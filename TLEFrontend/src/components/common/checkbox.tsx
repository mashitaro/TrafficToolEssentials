import styled from 'styled-components';

const Box = styled.div`
  border-width: 2rem;
  border-style: solid;
  border-color: rgba(100, 180, 255, 0.5);
  border-radius: 4rem;
  margin: 0 0.5em 0 0;
  width: 1.2em;
  height: 1.2em;
  padding: 2rem;
`;

const Checkmark = styled.div<{isChecked: boolean}>`
  width: 100%;
  height: 100%;
  mask-size: 100% auto;
  background-color: rgba(100, 180, 255, 1);
  opacity: ${props => props.isChecked ? 1 : 0};
`;

export default function Checkbox(props: {isChecked: boolean}) {
  return (
    <Box>
      <Checkmark isChecked={props.isChecked} style={{maskImage: "url(Media/Glyphs/Checkmark.svg)"}} />
    </Box>
  );
}
